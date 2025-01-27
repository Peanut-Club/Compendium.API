using System;

namespace Compendium.Voice.Profiles.Pitch;

public class PitchHelper
{
	public const int MaxFrameLength = 16000;

	private float[] gInFIFO = new float[16000];

	private float[] gOutFIFO = new float[16000];

	private float[] gFFTworksp = new float[32000];

	private float[] gLastPhase = new float[8001];

	private float[] gSumPhase = new float[8001];

	private float[] gOutputAccum = new float[32000];

	private float[] gAnaFreq = new float[16000];

	private float[] gAnaMagn = new float[16000];

	private float[] gSynFreq = new float[16000];

	private float[] gSynMagn = new float[16000];

	private long gRover;

	private long gInit;

	public void PitchShift(float pitchShift, long numSampsToProcess, float sampleRate, float[] indata)
	{
		PitchShift(pitchShift, numSampsToProcess, 2048L, 10L, sampleRate, indata);
	}

	public void PitchShift(float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp, float sampleRate, float[] indata)
	{
		long num = fftFrameSize / 2;
		long num2 = fftFrameSize / osamp;
		double num3 = (double)sampleRate / (double)fftFrameSize;
		double num4 = Math.PI * 2.0 * (double)num2 / (double)fftFrameSize;
		long num5 = fftFrameSize - num2;
		if (gRover == 0)
		{
			gRover = num5;
		}
		for (long num6 = 0L; num6 < numSampsToProcess; num6++)
		{
			gInFIFO[gRover] = indata[num6];
			indata[num6] = gOutFIFO[gRover - num5];
			gRover++;
			if (gRover < fftFrameSize)
			{
				continue;
			}
			gRover = num5;
			for (long num7 = 0L; num7 < fftFrameSize; num7++)
			{
				double num8 = -0.5 * Math.Cos(Math.PI * 2.0 * (double)num7 / (double)fftFrameSize) + 0.5;
				gFFTworksp[2 * num7] = (float)((double)gInFIFO[num7] * num8);
				gFFTworksp[2 * num7 + 1] = 0f;
			}
			ShortTimeFourierTransform(gFFTworksp, fftFrameSize, -1L);
			for (long num9 = 0L; num9 <= num; num9++)
			{
				double num10 = gFFTworksp[2 * num9];
				double num11 = gFFTworksp[2 * num9 + 1];
				double num12 = 2.0 * Math.Sqrt(num10 * num10 + num11 * num11);
				double num13 = Math.Atan2(num11, num10);
				double num14 = num13 - (double)gLastPhase[num9];
				gLastPhase[num9] = (float)num13;
				num14 -= (double)num9 * num4;
				long num15 = (long)(num14 / Math.PI);
				num15 = ((num15 < 0) ? (num15 - (num15 & 1)) : (num15 + (num15 & 1)));
				num14 -= Math.PI * (double)num15;
				num14 = (double)osamp * num14 / (Math.PI * 2.0);
				num14 = (double)num9 * num3 + num14 * num3;
				gAnaMagn[num9] = (float)num12;
				gAnaFreq[num9] = (float)num14;
			}
			for (int i = 0; i < fftFrameSize; i++)
			{
				gSynMagn[i] = 0f;
				gSynFreq[i] = 0f;
			}
			for (long num16 = 0L; num16 <= num; num16++)
			{
				long num17 = (long)((float)num16 * pitchShift);
				if (num17 <= num)
				{
					gSynMagn[num17] += gAnaMagn[num16];
					gSynFreq[num17] = gAnaFreq[num16] * pitchShift;
				}
			}
			for (long num18 = 0L; num18 <= num; num18++)
			{
				double num19 = gSynMagn[num18];
				double num20 = gSynFreq[num18];
				num20 -= (double)num18 * num3;
				num20 /= num3;
				num20 = Math.PI * 2.0 * num20 / (double)osamp;
				num20 += (double)num18 * num4;
				gSumPhase[num18] += (float)num20;
				double num21 = gSumPhase[num18];
				gFFTworksp[2 * num18] = (float)(num19 * Math.Cos(num21));
				gFFTworksp[2 * num18 + 1] = (float)(num19 * Math.Sin(num21));
			}
			for (long num22 = fftFrameSize + 2; num22 < 2 * fftFrameSize; num22++)
			{
				gFFTworksp[num22] = 0f;
			}
			ShortTimeFourierTransform(gFFTworksp, fftFrameSize, 1L);
			for (long num23 = 0L; num23 < fftFrameSize; num23++)
			{
				double num24 = -0.5 * Math.Cos(Math.PI * 2.0 * (double)num23 / (double)fftFrameSize) + 0.5;
				gOutputAccum[num23] += (float)(2.0 * num24 * (double)gFFTworksp[2 * num23] / (double)(num * osamp));
			}
			for (long num25 = 0L; num25 < num2; num25++)
			{
				gOutFIFO[num25] = gOutputAccum[num25];
			}
			for (long num26 = 0L; num26 < fftFrameSize; num26++)
			{
				gOutputAccum[num26] = gOutputAccum[num26 + num2];
			}
			for (long num27 = 0L; num27 < num5; num27++)
			{
				gInFIFO[num27] = gInFIFO[num27 + num2];
			}
		}
	}

	public static void ShortTimeFourierTransform(float[] fftBuffer, long fftFrameSize, long sign)
	{
		for (long num = 2L; num < 2 * fftFrameSize - 2; num += 2)
		{
			long num2 = 2L;
			long num3 = 0L;
			while (num2 < 2 * fftFrameSize)
			{
				if ((num & num2) != 0)
				{
					num3++;
				}
				num3 <<= 1;
				num2 <<= 1;
			}
			if (num < num3)
			{
				float num4 = fftBuffer[num];
				fftBuffer[num] = fftBuffer[num3];
				fftBuffer[num3] = num4;
				num4 = fftBuffer[num + 1];
				fftBuffer[num + 1] = fftBuffer[num3 + 1];
				fftBuffer[num3 + 1] = num4;
			}
		}
		long num5 = (long)(Math.Log(fftFrameSize) / Math.Log(2.0) + 0.5);
		long num6 = 0L;
		long num7 = 2L;
		for (; num6 < num5; num6++)
		{
			num7 <<= 1;
			long num8 = num7 >> 1;
			float num9 = 1f;
			float num10 = 0f;
			float num11 = (float)(Math.PI / (double)(float)(num8 >> 1));
			float num12 = (float)Math.Cos(num11);
			float num13 = (float)((double)sign * Math.Sin(num11));
			for (long num14 = 0L; num14 < num8; num14 += 2)
			{
				float num16;
				for (long num15 = num14; num15 < 2 * fftFrameSize; num15 += num7)
				{
					num16 = fftBuffer[num15 + num8] * num9 - fftBuffer[num15 + num8 + 1] * num10;
					float num17 = fftBuffer[num15 + num8] * num10 + fftBuffer[num15 + num8 + 1] * num9;
					fftBuffer[num15 + num8] = fftBuffer[num15] - num16;
					fftBuffer[num15 + num8 + 1] = fftBuffer[num15 + 1] - num17;
					fftBuffer[num15] += num16;
					fftBuffer[num15 + 1] += num17;
				}
				num16 = num9 * num12 - num10 * num13;
				num10 = num9 * num13 + num10 * num12;
				num9 = num16;
			}
		}
	}
}
